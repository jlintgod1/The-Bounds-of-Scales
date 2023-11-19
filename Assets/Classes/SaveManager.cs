// I took a lot of this code from one of my previous games(A Calm Nature,) as it
// has a lot of useful code for making save files. I could save my data in my player
// preferences, but it'll become a bit messy to manage.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public enum SaveLoadState
    {
        Success,
        FileNotFound,
        Corrupt,
    }
    // For unauthorized modification protection. We probably don't need it anyway because this is a game jam game!!
    public string dataChecksum;
    // Upgrades that we've bought
    public List<string> OwnedUpgrades = new List<string>();
    // Tutorials we've completed
    public List<string> CompletedTutorials = new List<string>();
    public int SnakeVenomCount;
    // In seconds
    public int TimeSpent;

    public Dictionary<string, bool> persistentVariables = new Dictionary<string, bool>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveData saveData = new SaveData();

#if UNITY_WEBGL
    // WebGL caches file changes, which is why we need this to push the changes immediately?
    [DllImport("__Internal")]
    public static extern void FlushFileWrites();
#endif
    public static void SaveSaveFile()
    {
        // File paths for the main save file along with its backups
        string filePath = Application.persistentDataPath + "/SaveFile.data";
        string backupFilePath = Application.persistentDataPath + "/SaveFile.data.bak";

        // Delete the first backup so that the save can be copied to its place
        if (File.Exists(backupFilePath)) { File.Delete(backupFilePath); }
        // Copy the save to the first backup
        if (File.Exists(filePath)) { File.Copy(filePath, backupFilePath); }

        // Open up a file stream and binary formatter
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(filePath, FileMode.Create);

        // Calculate a md5 hash for the save data. No reason, it was here to begin with.
        MD5 md5 = System.Security.Cryptography.MD5.Create();
        UTF8Encoding utf8 = new UTF8Encoding();
        string Checksum = BitConverter.ToString(
            md5.ComputeHash(
                utf8.GetBytes(
                    JsonUtility.ToJson(saveData.OwnedUpgrades).ToCharArray()
        )));
        saveData.dataChecksum = Checksum;


        // Serialize the save file
        formatter.Serialize(stream, saveData);
        // Close the file stream
        stream.Close();
#if UNITY_WEBGL
        FlushFileWrites();
#endif
    }

    public static SaveData.SaveLoadState LoadSaveFile()
    {
        string path = Application.persistentDataPath + "/SaveFile.data";
        // If the file exists
        if (File.Exists(path))
        {
            // Open a file stream and binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SaveData data = null;
            // Attempt to deserialize the file
            try
            {
                data = (SaveData)formatter.Deserialize(stream);
            }
            // If an exception is thrown...
            catch (System.Runtime.Serialization.SerializationException)
            {
                stream.Close();
                return SaveData.SaveLoadState.Corrupt;
            }
            // Close the file stream
            stream.Close();

            // Calculate a md5 hash for the ending data
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            UTF8Encoding utf8 = new UTF8Encoding();
            string endingChecksum = BitConverter.ToString(
                md5.ComputeHash(
                    utf8.GetBytes(
                        JsonUtility.ToJson(data.OwnedUpgrades).ToCharArray()
            )));
            // If the calculated hash doesn't match the saved hash (tampering might actually throw an exception)
            if (endingChecksum != data.dataChecksum)
            {
                // Sorry you cheated
                return SaveData.SaveLoadState.Corrupt;
            }

            // Save data exists
            saveData = data;
            return SaveData.SaveLoadState.Success;
        }
        else
        {
            // No save data exists
            return SaveData.SaveLoadState.FileNotFound;
        }
    }

    public static void DeleteSaveFile()
    {
        string path = Application.persistentDataPath + "/SaveFile.data";
        // If the file exists...
        if (File.Exists(path))
        {
            // DESTROY IT
            File.Delete(path);
        }
    }

    public static bool IsTutorialComplete(string name)
    {
        return saveData.CompletedTutorials.Contains(name);
    }

    public static void AddTutorial(string name)
    {
        if (IsTutorialComplete(name)) return;
        saveData.CompletedTutorials.Add(name);
    }
}
