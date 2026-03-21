# BetterDistanceEstimation

The game's built-in distance estimation visible on the power bar does not account for verticality. BetterDistanceEstimation does.

<img width="2381" height="1440" alt="image" src="https://github.com/user-attachments/assets/1aad30fb-1582-4ba0-a157-77a087252cf3" />

It's a small change: when the player and flag are at different heights, the height of the *flag* is used as the ground height for points that are closer to the flag than to the player.

## Changelog

### 0.2.1
Fix bugginess from last update if you point away from the flag

### 0.2.0
Fix inaccuracy near the cliff between the player and flag that created visible seam lines
