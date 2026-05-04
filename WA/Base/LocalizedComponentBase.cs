using Microsoft.AspNetCore.Components;
using WA.Services;

namespace WA.Base;

public class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject] private LayoutStateService LayoutState { get; set; } = default!;

    private Action? _handler;

    protected override void OnInitialized()
    {
        _handler = async () => await InvokeAsync(StateHasChanged);
        LayoutState.OnChange += _handler;
        base.OnInitialized();
    }

    public virtual void Dispose()
    {
        LayoutState.OnChange -= _handler;
    }
}