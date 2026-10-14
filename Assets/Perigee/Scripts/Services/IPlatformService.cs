namespace Perigee
{
    public interface IPlatformService
    {
        // меню впервые готово к нажатиям
        void GameReady();

        // вход в Playing и выход из него
        void GameplayStart();

        void GameplayStop();

        // язык браузера, две буквы
        string Language();

        // вибрация телефона, миллисекунды
        void Vibrate(int ms);
    }
}
