namespace SmartHomeApi.Models;

/// <summary>עדיפות משימה — הערכים המספריים משמשים למיון בשאילתות.</summary>
public enum TaskPriority
{
    Urgent = 30,
    Normal = 20,
    CanWait = 10
}

/// <summary>סטטוס משימה. לא נקרא TaskStatus כדי לא להתנגש עם System.Threading.Tasks.TaskStatus.</summary>
public enum HouseTaskStatus
{
    Pending = 0,
    Done = 1
}

/// <summary>תדירות חזרה של משימה.</summary>
public enum TaskRecurring
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

/// <summary>מי רואה את האלבום — כל המשפחה או רק הבעלים.</summary>
public enum AlbumVisibility
{
    Family = 0,
    Personal = 1
}

/// <summary>סוג תנועה בתקציב.</summary>
public enum BudgetType
{
    Income = 0,
    Expense = 1
}

/// <summary>סוג רשומה רפואית.</summary>
public enum MedicalRecordType
{
    Appointment = 0,
    Medication = 1,
    Vaccine = 2,
    Other = 3
}

/// <summary>מקור ההתראה.</summary>
public enum NotificationType
{
    Event = 0,
    Gift = 1,
    Budget = 2,
    Inventory = 3,
    Medication = 4,
    Vehicle = 5,
    Task = 6
}
