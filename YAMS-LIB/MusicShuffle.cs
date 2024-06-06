using System.Diagnostics.CodeAnalysis;

namespace YAMS_LIB;

public class MusicShuffle
{
    public static void ShuffleMusic(string musicDirectory, Dictionary<string, string> musicDict)
    {
        if (musicDict.Count == 0)
            return;


        DirectoryInfo musicPath = new DirectoryInfo(musicDirectory);

        // First we check whether we have a testers and itemamb2 music file. If we dont, we copy them
        if (!File.Exists(musicDirectory + "/mustester.ogg") && File.Exists(musicDirectory + "/musancientguardian.ogg"))
            File.Copy(musicDirectory + "/musancientguardian.ogg", musicDirectory + "/mustester.ogg");

        if (!File.Exists(musicDirectory + "/musitemamb2.ogg") && File.Exists(musicDirectory + "/musitemamb.ogg"))
            File.Copy(musicDirectory + "/musitemamb.ogg", musicDirectory + "/musitemamb2.ogg");

        // Then we rename all songs to have an _ at the end
        foreach (var musicFile in musicPath.GetFiles("*.ogg"))
            musicFile.MoveTo(musicFile.FullName + "_");

        // Then we go through each song in the dictionary
        foreach ((var newSongName, var origSongName) in musicDict)
        {
            if (File.Exists(musicDirectory + "/" + origSongName + "_"))
                File.Move(musicDirectory + "/" + origSongName + "_", musicDirectory + "/" + newSongName);
        }

        // Finally, if there are still songs with _ left, try to rename them back to what they should've been
        foreach (var musicFile in musicPath.GetFiles("*.ogg_"))
        {
            string origName = musicFile.FullName[..^1];
            if (!File.Exists(origName))
                musicFile.MoveTo(origName);
        }
    }
}
