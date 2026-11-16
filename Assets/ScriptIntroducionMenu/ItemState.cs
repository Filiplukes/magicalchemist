using System;
using System.Collections.Generic;

[Serializable]
public class ItemState
{
    public string Id;        // unikátní identifikátor
    public string Name;      // název zobrazovaný hráèi
    public int Price;        // cena

    public bool Owned;       // je koupené?
    public bool Active;      // je aktivní?

    public ItemState(string id, string name, int price,
                         bool owned = false,
                         bool active = false)
    {
        Id = id;
        Name = name;
        Price = price;
        Owned = owned;
        Active = active;
    }
}


[Serializable]
public class ItemStateCollection
{
    public List<ItemState> Items = new List<ItemState>();
}