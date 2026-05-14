using TechGems.StaticComponents;

namespace PokeBinder.Components;

public class HeroCardRowsComponent : StaticComponent
{

    public IReadOnlyList<IReadOnlyList<string?>>? RowImages { get; set; }
}
