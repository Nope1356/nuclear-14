using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype("marking")]
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; private set; }

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; private set; }

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; private set; }

        [DataField]
        public bool InvertSpeciesRestriction { get; private set; }

        [DataField]
        public Sex? SexRestriction { get; private set; }

        [DataField]
        public bool InvertSexRestriction { get; private set; }

        [DataField]
        public bool FollowSkinColor { get; private set; }

        [DataField]
        public bool ForcedColoring { get; private set; }

        [DataField]
        public MarkingColors Coloring { get; private set; } = new();

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;

        /// <summary>
        /// When true, this marking's sprite layers are appended to the very end of the entity's
        /// sprite layer stack, rendering above all clothing and armor.
        /// </summary>
        [DataField]
        public bool RenderOverClothing { get; private set; }

        /// <summary>
        /// When true, this marking is hidden from the character creator UI entirely.
        /// Use for system-applied markings that players should never select manually.
        /// </summary>
        [DataField]
        public bool Hidden { get; private set; }

        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }
    }
}
