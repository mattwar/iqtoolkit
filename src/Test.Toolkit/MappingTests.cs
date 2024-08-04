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
            var mapping = new AttributeMapping(typeof(AttributeContextTypes.AttributesOnEntity));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnEntity.Entity));
        }

        [TestMethod]
        public void TestAttributesOnContext()
        {
            var mapping = new AttributeMapping(typeof(AttributeContextTypes.AttributesOnContext));
            var entityMapping = mapping.GetEntity(typeof(AttributeContextTypes.AttributesOnContext.Entity));
        }

        private void TestAttributeMapping(Type contextType, Action<EntityMapping> fnCheck)
        {
            var mapping = new AttributeMapping(contextType);
            TestMapping(mapping);
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

        [TestMethod]
        public void TestNorthwindMapping()
        {
            TestMapping(new AttributeMapping(typeof(NorthwindWithAttributes)));
        }

        private void TestMapping(EntityMapping mapping)
        { 
            // walk through mapping info to prove that mapping is fully constructed
            // without throwing exceptions

            foreach (var member in mapping.ContextMembers)
            {
                var entity = mapping.GetEntity(member);
                WalkEntity(entity);
            }

            void WalkEntity(MappedEntity entity)
            {
                foreach (var table in entity.Tables)
                {
                    WalkTable(table);
                }

                foreach (var member in entity.MappedMembers)
                {
                    WalkMember(member);
                }
            }

            void WalkTable(MappedTable table)
            {
                if (table is MappedExtensionTable extensionTable)
                {
                    var related = extensionTable.RelatedTable;
                    var keyColumnNames = extensionTable.KeyColumnNames;
                    var relatedMembers = extensionTable.RelatedMembers;
                }
            }

            void WalkMember(MappedMember member)
            {
                switch (member)
                {
                    case MappedColumnMember column:
                        var colTable = column.Table;
                        break;
                    case MappedAssociationMember assoc:
                        var assocRelated = assoc.RelatedEntity;
                        var keys = assoc.KeyMembers;
                        var relatedKeys = assoc.RelatedKeyMembers;
                        break;
                    case MappedNestedEntityMember nested:
                        var nestedRelated = nested.RelatedEntity;
                        break;
                }
            }
        }
    }
}