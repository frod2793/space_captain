using System;
using System.Collections.Generic;

[Serializable]
public class LobbyDataDTO
{
    public string UID = "USER_777";
    public string ProfileIconID = "DefaultIcon";
    public string Nickname = "Captain";
    public int Level = 1;
    public int Gold = 0;
    public int Diamond = 0;
    public int CurrentStamina = 20;
    public int MaxStamina = 20;

    public List<string> OwnedCharacters = new List<string>();
    public List<string> DeckCharacters = new List<string>();
}
