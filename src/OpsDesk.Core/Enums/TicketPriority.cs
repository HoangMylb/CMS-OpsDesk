namespace OpsDesk.Core.Enums;

/// <summary>
/// The urgency level of a ticket. Drives SLA DueAt calculation.
/// SLA hours:  Low=72h  Medium=48h  High=24h  Critical=4h
/// Do NOT change the integer values — they are persisted to the database.
/// </summary>
public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
