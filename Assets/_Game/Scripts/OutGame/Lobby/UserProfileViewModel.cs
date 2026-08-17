using System;

public class UserProfileViewModel : IUserProfileViewModel
{
    private string m_uid;
    private string m_profileIconID;

    public string UID => m_uid;
    public string ProfileIconID => m_profileIconID;

    public event Action OnCloseRequested;

    public void SetData(string uid, string profileIconID)
    {
        m_uid = uid;
        m_profileIconID = profileIconID;
    }

    public void RequestClose()
    {
        OnCloseRequested?.Invoke();
    }
}
