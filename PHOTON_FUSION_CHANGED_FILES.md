# Photon Fusion Changed Files

This section summarizes the scripts changed during the Photon Fusion multiplayer setup. The code-side setup supports host/client internet sessions with lobby codes in the `AA00` format, while Unity-editor-only work such as importing Fusion, entering the App ID, and assigning Fusion prefab components still needs to be completed in Unity.

- `Assets/Scripts/UI/NetworkUI.cs`: Creates and joins Photon Fusion sessions using short lobby codes. Hosts generate hidden sessions, clients enter the exact code, and the runner is shut down cleanly when leaving.
- `Assets/Scripts/Network/PlayerMetadataRegistry.cs`: Stores submitted avatar selections by Fusion `PlayerRef`, syncs them through Fusion RPCs, tracks readiness, and moves the shared session into the game scene when all players are ready.
- `Assets/Scripts/Player/PlayerSpawner.cs`: Registers with the active Fusion runner, responds to player join/leave callbacks, spawns player network objects with input authority, and syncs infection/cure notifications.
- `Assets/Scripts/Player.cs`: Converts the player controller to a Fusion `NetworkBehaviour`, gates local control by input authority, and publishes movement/rotation to state authority.
- `Assets/Scripts/Player/PlayerCustoms.cs`: Converts avatar customization data into a Fusion `INetworkStruct` using network-safe fields such as `NetworkString<_64>` and `NetworkBool`.
- `Assets/Scripts/Player/PlayerVisual.cs`: Uses Fusion networked properties to replicate avatar appearance and infection visual state across clients.
- `Assets/Scripts/UI/PlayerUI.cs`: Converts interaction request and acknowledgement messages to Fusion RPCs targeted by `PlayerRef`.
- `Assets/Scripts/Network/ClientRegistry.cs`: Looks up player GameObjects by Fusion `PlayerRef` instead of Unity Netcode client IDs.
- `Assets/Scripts/Network/NetworkManagerLifetime.cs`: Keeps only one active Fusion `NetworkRunner` alive across scene loads.
- `Assets/Scripts/Camera/InteractGroupCamController.cs`: Uses Fusion input authority so only the owning player controls the interaction camera behavior.
- `Assets/Scripts/Avatar/AvatarGenerator.cs`: Builds Fusion-safe player customization data and submits it to the network metadata registry instead of loading the game scene locally.
- `Assets/Scripts/Utils/MetadataCustomsMapper.cs`: Converts between local `PlayerMetadata` and Fusion-safe `PlayerCustoms`, including safe `NetworkString<_64>` name handling.
- `Assets/Scripts/Player/PlayerLogbook.cs`: Stores player customization entries by stable player ID to prevent duplicate logbook entries after metadata resyncs.
- `Assets/Scripts/Memory/BubbleManager.cs`: Reads player display names from Fusion network strings and keys sequence lookups by player index instead of name.
- `Assets/Scripts/Anim/SequenceController.cs`: Uses Fusion scene loading when a runner exists, with a local scene-load fallback for non-networked use.
- `Assets/Scripts/Game/GameManager.cs`: Keeps game flow compatible with the updated Fusion player spawning path.
- `Assets/Scripts/Game/ASOGameManager.cs`: Keeps ASO game flow compatible with the updated Fusion player spawning path.

Remaining Unity setup: import Photon Fusion 2, configure the Photon App ID, add/register Fusion `NetworkObject` components on networked prefabs, remove old Unity Netcode scene objects/components, and verify all multiplayer scenes are included in Build Settings.
