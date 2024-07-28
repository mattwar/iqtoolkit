namespace Test.Toolkit
{
    using IQToolkit;
    using IQToolkit.Data;
    using IQToolkit.Data.Mapping;


    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void TestAttributesOnEntity()
        {
            var mapping = new AttributeMapping(typeof(AttributeContextTypes.AttributesOnEntity));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnEntity.Entity));
        }

        [TestMethod]
        public void TestAttributesOnContext()
        {
            var mapping = new AttributeMapping(typeof(AttributeContextTypes.AttributesOnContext));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnContext.Entity));
        }

        private void TestAttributeMapping(Type contextType, Action<AttributeMapping> fnCheck)
        {
            var mapping = new AttributeMapping(contextType);
            fnCheck(mapping);
        }

        internal static class AttributeContextTypes
        {
            internal abstract class AttributesOnEntity
            {
                [Table]
                public class Entity
                {
                    [Column(IsPrimaryKey = true)]
                    public int Key { get; set; } = default!;

                    [Column]
                    public string Value { get; set; } = default!;
                }

                public abstract IEntityTable<Entity> Entities { get; }
            }

            internal abstract class AttributesOnContext
            {
                public class Entity
                {
                    public int Key { get; set; } = default!;
                    public string Value { get; set; } = default!;
                }

                [Table]
                [Column(Member = nameof(Entity.Key), IsPrimaryKey = true)]
                [Column(Member = nameof(Entity.Value))]
                public abstract IEntityTable<Entity> Entities { get; }
            }

        }
    }
}