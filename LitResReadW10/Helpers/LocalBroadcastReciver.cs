using System.ComponentModel;

namespace LitResReadW10.Helpers
{
    public class LocalBroadcastReciver: INotifyPropertyChanging
    {
        private LocalBroadcastReciver()
        {
        }

        public static LocalBroadcastReciver Instance { get; } = new LocalBroadcastReciver();

        public void OnPropertyChanging(object sender, PropertyChangingEventArgs eventArgs)
        {
            PropertyChanging?.Invoke(sender, eventArgs);
        }

        public event PropertyChangingEventHandler PropertyChanging;
    }
}
