# BetterDistanceEstimation

The game's built-in distance estimation visible on the power bar does not account for verticality. BetterDistanceEstimation does.

<img width="2381" height="1440" alt="image" src="https://github.com/user-attachments/assets/066fce2e-4cc7-4e4a-984b-40faa952ffff" />

It's a small change: when the player and flag are at different heights, the height of the *flag* is used as the ground height for points that are closer to the flag than to the player.
