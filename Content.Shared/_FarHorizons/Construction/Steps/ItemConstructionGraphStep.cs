namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class ItemConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("item")]
        private string? _item;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            if (string.IsNullOrEmpty(_item))
                return false;

            var meta = entityManager.GetComponentOrNull<MetaDataComponent>(uid);
            return meta?.EntityPrototype?.ID == _item;
        }
    }
}