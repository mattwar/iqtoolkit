namespace Test.Toolkit
{
    using IQToolkit;
    using IQToolkit.Entities;
    using IQToolkit.Entities.Mapping;


    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void TestAttributesOnEntity()
        {
            var mapping = new AttributeEntityMapping(typeof(AttributeContextTypes.AttributesOnEntity));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnEntity.Entity));
        }

        [TestMethod]
        public void TestAttributesOnContext()
        {
            var mapping = new AttributeEntityMapping(typeof(AttributeContextTypes.AttributesOnContext));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnContext.Entity));
        }

        private void TestAttributeMapping(Type contextType, Action<AttributeEntityMapping> fnCheck)
        {
            var mapping = new AttributeEntityMapping(contextType);
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