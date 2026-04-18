using Microsoft.JSInterop;

namespace CorpLinkBaseMinimal.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        public static event Action? OnThemeChanged;
        private static bool _isDarkMode = false;
        private static bool _isInitialized = false;

        public bool IsDarkMode => _isDarkMode;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task LoadThemeAsync()
        {
            // Если уже загрузили, не загружаем снова
            if (_isInitialized) return;

            try
            {
                var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "darkMode");
                _isDarkMode = theme == "true";
                _isInitialized = true;
                OnThemeChanged?.Invoke();
            }
            catch
            {
                _isDarkMode = false;
                _isInitialized = true;
            }
        }

        public async Task ToggleThemeAsync()
        {
            _isDarkMode = !_isDarkMode;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "darkMode", _isDarkMode.ToString());
            OnThemeChanged?.Invoke();
        }

        // Метод для принудительной синхронизации темы на странице
        public async Task SyncThemeAsync()
        {
            try
            {
                var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "darkMode");
                var newIsDark = theme == "true";
                if (_isDarkMode != newIsDark)
                {
                    _isDarkMode = newIsDark;
                    OnThemeChanged?.Invoke();
                }
            }
            catch { }
        }
    }
}