using JournalApp.Models;

namespace JournalApp.Services
{
    public class StateContainer
    {
        public event Action OnChange;
        
        private UserProfileDto _currentUser;
        private bool _isAuthenticated = false;

        public UserProfileDto CurrentUser 
        { 
            get => _currentUser; 
            private set 
            { 
                _currentUser = value;
                NotifyStateChanged();
            } 
        }

        public bool IsAuthenticated 
        { 
            get => _isAuthenticated; 
            private set 
            { 
                _isAuthenticated = value;
                NotifyStateChanged();
            } 
        }

        public void SetCurrentUser(UserProfileDto user)
        {
            CurrentUser = user;
            IsAuthenticated = user != null;
        }

        public void ClearCurrentUser()
        {
            CurrentUser = null;
            IsAuthenticated = false;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}