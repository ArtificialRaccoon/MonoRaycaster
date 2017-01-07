# MonoRaycaster

Ported and Forked from: http://lodev.org/cgtutor/raycasting.html

Yet another Raycaster based off of Lode's original raycasting code.  This is a C# implimentation, complatible with Mono.  Lode's method for floorcasting was discarded, and instead I opted to port Amarillion's "MODE7" example instead (http://www.helixsoft.nl/articles/circle/sincos.htm).
	 
A pretty lame/simple fog effect is implimented.  The farther away something is, the darker it is.  This gives the impression of minimal light.

Lode's orignal code has had some minor changes made to improve performance with C# (Overall, the changes are minimal).

OpenGL is used to simply scale the output quickly.  You could drop this completely if you applied to to a canvas in WPF for example.

I was going to add schema validation, and add a basic map editor, but I decided I wasn't very interested in this any further.
