# The Disaffected Dice

The idea is simple: you play as a dice. You move around with the arrow keys or the left stick. You can only move on tiles that match the top number of the dice, or else you'll die. In order to win, you have to make it to the green tile.
Expanding on this idea, there could be moving platforms, blocks with numbers that change according to the music, and spikes that kill you if you stay on a tile for too long.

Unfortunately, I didn't end up getting to implement most of this, only because Unity doesn't provide any simple out-of-the-box way to access arbitrary game objects through a tilemap. Just now (around an hour before the end of the jam) I managed to finish writing my own wrapper class over UnityEngine.Tilemaps.Tilemap. Given the fact that I just started using Unity, it is pretty phenomenal that I managed to figure out how to do it, so I don't feel too bad about it.