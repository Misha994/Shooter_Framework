using static Unity.VisualScripting.Icons;

public interface ILocalizationService
{
    string Get(string key);
    void SetLanguage(Language lang);
}
