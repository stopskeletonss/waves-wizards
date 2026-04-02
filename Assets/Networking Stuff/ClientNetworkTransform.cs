using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    //Sync position of gameobject through client
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
