using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Kanelson.Components;

public class ServerModelValidator : ComponentBase
{
    private ValidationMessageStore _messageStore = null!;

    [CascadingParameter] EditContext CurrentEditContext { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(ServerModelValidator)} requires a cascading parameter " +
                                                $"of type {nameof(EditContext)}. For example, you can use {nameof(ServerModelValidator)} inside " +
                                                $"an {nameof(EditForm)}.");
        }

        _messageStore = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += (s, e) => _messageStore.Clear();
        CurrentEditContext.OnFieldChanged += (s, e) => _messageStore.Clear(e.FieldIdentifier);
    }

    public void DisplayErrors(Dictionary<string, List<string>> errors)
    {
        foreach (var err in errors)
        {
            _messageStore.Add(CurrentEditContext.Field(err.Key), err.Value);
        }
        CurrentEditContext.NotifyValidationStateChanged();
    }


    public void DisplayError(string field, string validationMessage)
    {
        var dictionary = new Dictionary<string, List<string>>
        {
            { field, new List<string> { validationMessage } }
        };

        DisplayErrors(dictionary);
    }


}