using System;

public interface IUserProfileViewModel
{
    string UID { get; }
    string ProfileIconID { get; }
    
    event Action OnCloseRequested;
    
    void RequestClose();
}
