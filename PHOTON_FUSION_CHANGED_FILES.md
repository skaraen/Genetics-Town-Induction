# Photon Fusion Changed Files

- NetworkUI: Creates and joins Photon Fusion sessions using short lobby codes. Hosts generate hidden sessions, clients enter the exact code, and the runner is shut down cleanly when leaving.
- PlayerMetadataRegistry: Stores submitted avatar selections by Fusion PlayerRef, syncs them through Fusion RPCs, tracks readiness, and moves the shared session into the game scene when all players are ready.
- PlayerSpawner: Registers with the active Fusion runner, responds to player join/leave callbacks, spawns player network objects with input authority, and syncs infection/cure notifications.
- Player: Converts the player controller to Fusion Network Behavior, adds gated local control, and pushes movement/rotation to state authority.
- PlayerCustoms: Converts avatar customization data into syntax that is compatibl with Fusion.
- PlayerVisual: Syncs avatar appearance and infection visuals between clients.
- PlayerUI: Converts interaction request and acknowledgement messages to Fusion RPCs.
- ClientRegistry: Looks up player GameObjects by Fusion PlayerRef instead of Unity Netcode client IDs.
- NetworkManagerLifetime: Keeps only one active Fusion network runner alive across scene loads.
- InteractGroupCamController: Uses Fusion authority to make it so that only the host client controls the camera behavior.
- AvatarGenerator: Builds Fusion-safe player customization data and submits it to the network metadata registry instead of loading the game scene locally.
- MetadataCustomsMapper: Converts between local player metadata and fusion format (PlayerCustoms).
- PlayerLogbook: Stores player customization entries. Uses player ID to prevent duplicate logbook entries when resyncing.
- BubbleManager: Reads player display names from Fusion network strings and stores sequence lookups by player index instead of name.
- SequenceController: Uses Fusion scene loading when a runner exists, with a local scene-load fallback for non-networked use.
- GameManager: Keeps game flow (specifically player spawning) in sync using Fusion syntax
