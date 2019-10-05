# LINQ: Building an IQueryable Provider – Part III: Local variable references

Matt Warren - MSFT; August 1, 2007

---

This post is the third in a series of posts covering how to build a LINQ IQueryable provider. If you have not read the previous posts, please do so before proceeding.


Complete list of posts in the Building an IQueryable Provider series 


Part III?  Wasn’t I done in the last post? Didn’t I have the provider actually working, translating, executing and returning a sequence of objects? 


Sure, that’s true, but only just so. The provider I built was really fragile. It only understood one major query operator and a few minor ones like comparison, etc. However, real providers are going to have to deal with many more operators and complicated interactions between them. For example, that provider did not even let you project the data into new shapes. How one goes about doing that is non-obvious.


Translating Local Variable References


Did you see what happens when the query references local variables? No?


string city = "London";


var query = db.Customers.Where(c => c.City == city);


 


What happens when we try to translate that? Go ahead try it. I’m waiting.


Bam! You get an exception, “The member 'city' is not supported.”  What does that mean? I thought the ‘member’ city was one of the columns.  Well, it is.  What the exception is referring to is the local variable ‘city’.  Yet, how is that a ‘member’?


Take a look at the ToString() translation of the expression tree.


Console.WriteLine(query.Expression.ToString());


 


Here’s what you get:


SELECT * FROM Customers.Where(c => return (c.City = value(Sample.Program+<>c__DisplayClass0).city))


Aha!  The c# compile has made a class to hold local variables that are being referenced in the lambda expression.  This is the same thing it does when local variables are referenced inside an anonymous method. But you already knew that. Didn’t you? No?


Regardless, if I want to have my provider work with references to local variables I’m going to have to deal with it.  Maybe I can just recognize field references to these compiler generated types? How do I identify a compiler generated type? By name? What if the c# compiler changes how they name them? What if another language uses a different scheme?  Are local variables the only interesting case? What about references to member variables in scope at the time? Those aren’t going to be encoded as values in the tree either are they? At best they will be a constant node referencing the instance the member is on and then a MemberAccess node that accesses the member off that instance.  Can I just recognize any member access against a constant node and evaluated it by hand using reflection?  Maybe.  What if the compiler generates something more complicated?


Okay, what I’m going to give you is a general purpose solution that will turn these compiler generated trees into something much more palatable, more like the trees you thought you were getting before I pointed out this mess.


What I really want to do is identify sub-trees in the overall tree that can be immediately evaluated and turned into values.  If I can do this, then the rest of the translator only needs to deal with these values. Thank goodness I already have the ExpressionVisitor defined. I can use that and invent a pretty simple rule to determine what is sub-tree that can be evaluated locally.


Take a look at the code first and then I’ll explain how it does what it does.


public static class Evaluator {


    /// <summary>


    /// Performs evaluation & replacement of independent sub-trees


    /// </summary>


    /// <param name="expression">The root of the expression tree.</param>


    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>


    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>


    public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) {


        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);


    }


 


    /// <summary>


    /// Performs evaluation & replacement of independent sub-trees


    /// </summary>


    /// <param name="expression">The root of the expression tree.</param>


    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>


    public static Expression PartialEval(Expression expression) {


        return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);


    }


 


    private static bool CanBeEvaluatedLocally(Expression expression) {


        return expression.NodeType != ExpressionType.Parameter;


    }


 


    /// <summary>


    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)


    /// </summary>


    class SubtreeEvaluator: ExpressionVisitor {


        HashSet<Expression> candidates;


 


        internal SubtreeEvaluator(HashSet<Expression> candidates) {


            this.candidates = candidates;


        }


 


        internal Expression Eval(Expression exp) {


            return this.Visit(exp);


        }


 


        protected override Expression Visit(Expression exp) {


            if (exp == null) {


                return null;


            }


            if (this.candidates.Contains(exp)) {


                return this.Evaluate(exp);


            }


            return base.Visit(exp);


        }


 


        private Expression Evaluate(Expression e) {


            if (e.NodeType == ExpressionType.Constant) {


                return e;


            }


            LambdaExpression lambda = Expression.Lambda(e);


            Delegate fn = lambda.Compile();


            return Expression.Constant(fn.DynamicInvoke(null), e.Type);


        }


    }


 


    /// <summary>


    /// Performs bottom-up analysis to determine which nodes can possibly


    /// be part of an evaluated sub-tree.


    /// </summary>


    class Nominator : ExpressionVisitor {


        Func<Expression, bool> fnCanBeEvaluated;


        HashSet<Expression> candidates;


        bool cannotBeEvaluated;


 


        internal Nominator(Func<Expression, bool> fnCanBeEvaluated) {


            this.fnCanBeEvaluated = fnCanBeEvaluated;


        }


 


        internal HashSet<Expression> Nominate(Expression expression) {


            this.candidates = new HashSet<Expression>();


            this.Visit(expression);


            return this.candidates;


        }


 


        protected override Expression Visit(Expression expression) {


            if (expression != null) {


                bool saveCannotBeEvaluated = this.cannotBeEvaluated;


                this.cannotBeEvaluated = false;


                base.Visit(expression);


                if (!this.cannotBeEvaluated) {


                    if (this.fnCanBeEvaluated(expression)) {


                        this.candidates.Add(expression);


                    }


                    else {


                        this.cannotBeEvaluated = true;


                    }


                }


                this.cannotBeEvaluated |= saveCannotBeEvaluated;


            }


            return expression;


        }


    }


}


The Evaluator class exposes a static method ‘PartialEval’ that you can call to evaluate these sub-trees in your expression, leaving only constant nodes with actual values in their place. 


The majority of this code is the demarking of maximal sub-trees that can be evaluated in isolation. The actual evaluation is trivial since the sub-trees can be ‘compiled’ using LambaExpression.Compile, turned into a delegate and then invoked.  You can see this happening in the SubtreeVisitor.Evaluate method.


The process of determining maximal sub-trees happens in two steps.  First by a bottom-up walk in the Nominator class that determines which nodes could possibly be evaluated in isolation and then a top-down walk in SubtreeEvaluator that finds highest nodes representing sub-trees that were nominated.


The Nominator is parameterized by a function that you supply that can employ whatever heuristics you want to determine if some given node can be evaluated in isolation. The default heuristic is that any node except ExpresssionType.Parameter can be evaluated in isolation.  Beyond that, a general rule states that if a child node cannot be evaluated locally then the parent node cannot either.  Therefore, any node upstream of a parameter cannot be evaluated and will remain in the tree. Everything else will be evaluated and replaced as constants. 


Now that I have this class I can put it to work by using it whenever I go about translating expression trees.  Fortunately, I already have this operation factored out into the ‘Translate’ method on the DbQueryProvider class.


public class DbQueryProvider : QueryProvider {
    …


    private string Translate(Expression expression) {


        expression = Evaluator.PartialEval(expression);


        return new QueryTranslator().Translate(expression);


    }


}



Now if we try the following code we get a better result:


string city = "London";


var query = db.Customers.Where(c => c.City == city);


 


Console.WriteLine("Query:\n{0}\n", query);



Which writes:


Query:
SELECT * FROM (SELECT * FROM Customers) AS T WHERE (City = 'London')


Exactly what I wanted.  This provider is coming along nicely! 


Maybe next time I’ll implement Select.