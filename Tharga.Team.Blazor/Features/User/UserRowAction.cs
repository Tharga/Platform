namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// A consumer-supplied split-button action on a user row: the clicked item's <c>Value</c> and the
/// row's user. Delivered via the <c>ActionInvoked</c> callback for items added through
/// <c>ActionItems</c>.
/// </summary>
/// <param name="Action">The <c>Value</c> of the clicked <c>RadzenSplitButtonItem</c>.</param>
/// <param name="User">The user row the action was invoked on.</param>
public sealed record UserRowAction(string Action, UserViewModel User);
