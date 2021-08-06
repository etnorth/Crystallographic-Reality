using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTag : MonoBehaviour
{
    //[SerializeField] // Not sure what this does so removed it
    public List<string> tags = new List<string>(); // Was private, but changed for adding tags in Crystal.cs/CreateCrystal add corner/edge/face atom

    public bool HasTag(string tag)
    {
        return tags.Contains(tag);
    }

    public IEnumerable<string> GetTags()
    {
        return tags;
    }

    public void Rename(int index, string tagName)
    {
        tags[index] = tagName;
    }

    public string GetAtIndex(int index)
    {
        return tags[index];
    }

    public int Count
    {
        get { return tags.Count; }
    }

    public void AddTag(string tagName)
    {
        tags.Add(tagName);
    }
}
