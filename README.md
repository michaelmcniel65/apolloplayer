<h1>Apollo Player v1.1.07</h1>

Last Updated: 12 April 2024 9:32 PM CST

Any questions or bug reports should be emailed to michaelcmcniel@gmail.com

Thank you!

![apollo_player](https://github.com/michaelmcniel65/apolloplayer/assets/100385832/af85e666-cd56-4846-a1bb-0dc8b46e57bc)

<h2>Demo Video</h2>
https://www.youtube.com/watch?v=Cyf2IESyITs

<h2>About</h2>
Apollo Player is a music player written in C# using NAudio, a .NET audio library from Mark Heath (https://github.com/naudio/NAudio) in WPF.

The entire program is written in C# using 900 lines of code. It averages 150 MB of memory usage when playing a song. I did my absolute best to keep as much memory cleared as possible and use good rules of thumb when it comes to creating a WPF application. The goal was to create a music player that did not depend on the included Windows Media Player library in order to challenge myself and make something that I could potentially port over to Linux. The real grind was making NAudio work with WPF as it was initially made for WinForms. Luckily, NAudio proved to work extremely well and I'm able to deliver one of my coolest projects to date.

CHANGE LOG
----------

1.0.00
- Initial Build

1.0.01
- Fixed a bug where the app would freeze if the play button was pressed twice

1.0.02
- Changed music file folder to the built in Windows Music folder
- Fixed bug where the next song wouldn't play automatically

1.0.03
- Fixed a bug where the app would crash if a song that is playing is deleted

1.0.04
- Fixed a bug where songs would overlap after playing a different song if no song was selected (the default song would overlap
	the currently playing song)
1.0.05
- Fixed a bug where the app would crash after pausing then playing a song

1.1.06
- Fixed a bug where the app would crash if the pause button was clicked and no song was playing
- Stylized the song list scroll bar
- Added the ability for the current song display to scroll from right to left to show full name of file
- Formatted the names of the songs in the song list to include "..." if the song is too long

1.1.07
- Fixed a bug where app would freeze after 20 minutes of use. Deleted line of code that allowed only software rendering
- Set the volume to be max on start
- Changed the formatted text to be 2 characters shorter
- Set the music list font to the included font resource to show up on other computers
