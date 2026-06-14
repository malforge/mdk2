namespace IngameScript {
    public class Utility {
        // Dummy utility class with enum to demonstrate unreferenced enum inclusion
        public string someMethod() {
            return "Goodbye World";
        }
    }
    public enum PingState {
        Idle,
        Waiting,
        Success,
        Failed,
        TimedOut
    }
}