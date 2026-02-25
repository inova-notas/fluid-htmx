using Fluid;
using Fluid.Ast;
using Parlot.Fluent;

namespace InovaNotas.FluidHtmx.Htmx;

public class HtmxFluidParser : FluidParser
{
    public Parser<IReadOnlyList<FilterArgument>> ArgumentsListParser => ArgumentsList;
}
