using System.Threading.Tasks;
using System.Windows.Input;


namespace MedReminder.ViewModels
{
    public static class CommandExtensions
    {
        public static Task ExecuteAsync(this ICommand command)
        {
            var tcs = new TaskCompletionSource();
            command.Execute(null);
            tcs.SetResult();
            return tcs.Task;
        }


        public static Task ExecuteAsync<T>(this ICommand command, T param)
        {
            var tcs = new TaskCompletionSource();
            command.Execute(param);
            tcs.SetResult();
            return tcs.Task;
        }
    }
}