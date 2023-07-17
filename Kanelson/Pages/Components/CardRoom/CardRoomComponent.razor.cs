using Microsoft.AspNetCore.Components;

namespace Kanelson.Pages.Components.CardRoom;

public partial class CardRoomComponent : ComponentBase
{
    [Parameter] 
    public string RoomName { get; set; } = null!;
     
    [Parameter]
    public long Id { get; set; }

    [Parameter]
    public string HostName { get; set; } = null!;
    
    [Parameter]
    public EventCallback EnterRoomClick { get; set; }

    
    private async Task HandleButtonClick()
    {
        await EnterRoomClick.InvokeAsync();
    }
}