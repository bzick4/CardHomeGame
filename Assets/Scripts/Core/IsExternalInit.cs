// Unity компилирует C# 9, но не содержит этот служебный тип — без него не работают свойства с `init`
// (значение задаётся только при создании объекта: new GuestDef { Name = … }).
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
