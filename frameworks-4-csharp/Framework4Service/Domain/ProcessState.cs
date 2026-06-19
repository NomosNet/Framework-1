namespace Framework4Service.Domain;

public static class ProcessState
{
    public const string New = "Новый";
    public const string ApplicationAccepted = "ЗаявкаПринята";
    public const string ResourceReserved = "РесурсЗабронирован";
    public const string AccessGranted = "ДоступВыдан";
    public const string Completed = "Завершён";
    public const string CompensationDone = "КомпенсацияВыполнена";
    public const string Error = "Ошибка";
}
