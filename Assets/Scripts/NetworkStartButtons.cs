
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class NetworkStartButtons : MonoBehaviour
{
    [SerializeField] private int maxJoiningPlayers = 3;
    private UnityTransport transport;
    private string joinCode = "";
    private string enteredJoinCode = "";
    private string status = "Not connected";
    private bool isConnecting = false;


    private void Awake()
    {
        if (NetworkManager.Singleton == null) // make sure the network manager exists
            return;

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private void Update()
    {
        // copy the join code when C is pressed, as long as a code exists
        if (Input.GetKeyDown(KeyCode.C) &&
            !string.IsNullOrEmpty(joinCode) &&
            GUI.GetNameOfFocusedControl() != "JoinCodeInput")
        {
            GUIUtility.systemCopyBuffer = joinCode;
            status = "Join code copied to clipboard!";
        }
    }

    private void OnGUI()
    {
        // display the current connection status
        GUI.Label(new Rect(10, 10, 400, 25),
            "Status: " + status);

        GUI.enabled = !isConnecting && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening;

        // create a Relay session and start the host
        if (GUI.Button(new Rect(10, 40, 180, 40), "Start Host"))
            StartHostWithRelay();

        // indicator to enter the join code provided by the host
        GUI.Label(new Rect(10, 95, 180, 25), "Enter Join Code:");

        GUI.SetNextControlName("JoinCodeInput");
        enteredJoinCode = GUI.TextField(new Rect(10, 120, 180, 30), enteredJoinCode); // input field

        // connect to the host using the inputted join code
        if (GUI.Button(new Rect(10, 160, 180, 40), "Join Game"))
            JoinGameWithRelay();

        GUI.enabled = true;

        // show the code that other players need to be able to join
        if (!string.IsNullOrEmpty(joinCode))
        {
            GUI.Label(new Rect(10, 215, 400, 25),
                "Share this code: " + joinCode);

            GUI.Label(new Rect(10, 245, 400, 25),
                "Press C to copy the join code");
        }
    }


    // called when the Start Host button is pressed
    private async void StartHostWithRelay()
    {
        if (isConnecting || transport == null)
            return;

        isConnecting = true;
        status = "Initializing services...";

        try
        {
            await InitializeServices();
            status = "Creating Relay allocation...";

            // allocate slots for remote players
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJoiningPlayers);

            // configure Unity Transport to use Relay
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            // generate the code that clients will enter
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (!NetworkManager.Singleton.StartHost())
                throw new Exception("Failed to start host.");

            status = "Hosting game";
        }
        catch (Exception e)
        {
            status = "Host failed: " + e.Message;
            Debug.LogException(e);
        }
        finally
        {
            isConnecting = false;
        }
    }


    // called when the Join Game button is pressed
    private async void JoinGameWithRelay()
    {
        if (isConnecting || transport == null)
            return;

        string code = enteredJoinCode.Trim(); // get rid of any spaces after the code

        if (string.IsNullOrWhiteSpace(code))
        {
            status = "Please enter a join code.";
            return;
        }

        isConnecting = true;

        try
        {
            await InitializeServices();

            // join the host's existing Relay allocation
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);

            // configure the client to connect through Relay
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            if (!NetworkManager.Singleton.StartClient())
                throw new Exception("Failed to start client.");

            status = "Connecting to host...";
        }
        catch (Exception e)
        {
            status = "Join failed: " + e.Message;
            Debug.LogException(e);
        }
        finally
        {
            isConnecting = false;
        }
    }


    private async Task InitializeServices()
    {
        // initialize Unity Services once per application session
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        // sign in without requiring the player to create an account
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}
