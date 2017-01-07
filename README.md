# MonoRaycaster

Yet another Raycaster based off of Lode's original raycasting code.  This is a C# implimentation, complatible with Mono.  A Textured Raycaster written in managed C#.  Lode's method for floorcasting was discarded, and instead I opted to port Amarillion's "MODE7" example instead (http://www.helixsoft.nl/articles/circle/sincos.htm).
	 
A pretty lame/simple fog effect is implimented.  The farther away something is, the darker it is.  This gives the impression of minimal light.

The end result is a fairly speedy C# based raycaster.
