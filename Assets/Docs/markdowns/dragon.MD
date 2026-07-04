# Technical Development Specification: Crystal Dragon (Hybrid/Heavy)

## 1. Mob Overview
*   **Name:** Crystal Dragon
*   **Type:** Common Mob (Hybrid: Terrestrial / Temporary Flyer)
*   **Gameplay Style:** Heavy patrol combatant. It compensates for its poor flight capabilities by alternating between ground-based projectile attacks and clumsy aerial dashes that deal direct contact damage.
*   **Flight Behavior:** It initiates a forced flight toward the player. However, due to the heavy weight of its crystal structure, it quickly loses altitude and crashes into the ground or the target. It requires a cooldown period on the ground before attempting to fly again.

## 2. Attributes and Base Stats
*   **Ground Speed:** 5
*   **Aerial Dash Speed:** 8 (Fast advance)
*   **General Base Damage:** 8
*   **Contact Damage (Dash/Crash):** 15
*   **Player Detection Radius:** 8 meters
*   **Flight Cooldown:** X seconds (Time required to remain on the ground after crashing).

## 3. Behavior and Artificial Intelligence (FSM / State Machine)

*   **State: Terrestrial Patrol (Patrol)**
    *   Uses `NavMeshAgent` to patrol short distances. Scan radius: 8 meters.
*   **State: Terrestrial Chase (Chase)**
    *   Upon detecting the player, advances at speed 5 using the `NavMeshAgent`.
    *   Alternates between **Crystal Spikes** (if distant and facing the player) or **Tail Sweep** (if the player flanks sides/rear).
    *   **Heavy Crash Trigger:** If the Flight Cooldown is at zero **AND** the distance to the player is less than or equal to **4 meters**, it interrupts the chase and transitions to the **Flight Preparation** state.
*   **State: Flight Preparation (Charge/Telegraph)**
    *   The dragon stops moving immediately (`navMeshAgent.isStopped = true`).
    *   Enters a "charging/telegraph" animation (e.g., slamming wings heavily on the ground, gathering crystalline energy) for a set duration (e.g., 1 to 1.5 seconds).
    *   Locks onto the player's last known position at the end of the charge.
*   **State: Aerial Dash (Heavy Crash)**
    *   Mechanics: Disables the `NavMeshAgent`, enables the `Rigidbody`, and launches itself at speed 8 (parabolic/low-altitude arc) toward the locked player position.
*   **State: Crash and Recovery (Crash / Recovery)**
    *   Upon colliding with the player, a wall, or the ground, it crashes heavily.
    *   Remains stunned/immobile for 1–2 seconds (vulnerability window for the player).
    *   Re-enables the `NavMeshAgent`, returns to Terrestrial Chase, and triggers the Flight Cooldown timer.

## 4. Skills and Attack Logic

### A. Tail Sweep (Terrestrial Spin Attack)
*   **Condition:** Player is at melee range, positioned either to the sides or behind the Dragon while it is on the ground.
*   **Damage:** 15 | **Cooldown:** 2 seconds.

### B. Crystal Spikes (Frontal Ranged Attack)
*   **Condition:** Player is at a distance and facing the front of the Dragon on the ground.
*   **Execution:** Fires 3 frontal projectiles in a straight line. Damage = 8 per projectile hit.

### C. Heavy Crash (4m Trigger with Telegraph)
*   **Activation Distance:** 4 meters from the player.
*   **Telegraph/Charge Time:** ~1.2 seconds.
*   **Attack Execution:** The body enables its contact damage hitbox. Advances as a heavy physics object.
*   **Contact Damage:** 15 + Knockback.

## 5. Integration and Physics Requirements in Unity
*   **Required Components:** `NavMeshAgent` (for ground mode) and `Rigidbody` + `Collider` (for flight/collision mode).
*   **Ground -> Flight Transition:** 
    ```csharp
    navMeshAgent.enabled = false;
    rigidbody.isKinematic = false;
    // Apply upward and forward force toward the locked player position