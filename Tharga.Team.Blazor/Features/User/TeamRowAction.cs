namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// A consumer-supplied split-button action on a team row: the clicked item's <c>Value</c> and the row's
/// team. Delivered via the <c>TeamActionInvoked</c> callback for items added through
/// <c>TeamActionItems</c>. The Teams-tab counterpart of <see cref="UserRowAction"/>.
/// </summary>
/// <param name="Action">The <c>Value</c> of the clicked <c>RadzenSplitButtonItem</c>.</param>
/// <param name="Team">The team row the action was invoked on.</param>
public sealed record TeamRowAction(string Action, TeamViewModel Team);
