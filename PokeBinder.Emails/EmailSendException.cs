namespace PokeBinder.Emails;

/// <summary>A sender that would not take an email. The message says which sender and why.</summary>
public sealed class EmailSendException(string message, Exception? inner = null) : EmailException(message, inner);
