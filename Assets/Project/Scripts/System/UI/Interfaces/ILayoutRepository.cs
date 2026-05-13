using System.Collections.Generic;

namespace Project.Scripts.Systems.UI
{
    public interface ILayoutRepository
    {
        List<LayoutViewBase> Views { get; }
    }
}
