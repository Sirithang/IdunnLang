IdunnLang
=========

Simple scripting language mapping to JSON.

The language is aimed at defining simple event that modify a collection of objects that are a list of clefs-values. 
Each object is based on an archetype, that can be define (think as archetype as "default object".)

It's first intended usage is to be used in event-driven narrative game (i.e. "Choose Your Own Adventure") but could be used for other purpose.

IdunnParser
-----------

Each parser is a simple VM that keep its object hierarchy, and allow for quiering and executing scripts (see events in doc).

The language don't aim at replacing more advance scripting language like lua or javascript, but simply offer a simpler, easier to integrate alternative.
Basic parsers is in C# but specs are simple enough to reimplement it in pure C so it can be integrated in anything through IFF. Futur work could be done in that direction.

IdunnIDE
--------

The IDE is a full application aimed at simplifying creation/modification of archetype/events.

It offer simple database functionnality (search) throught all archetypes/events, and export it into format that a parser can be feed.
It also offer simple coloration & autocompletion(archetype fields,builtin func) for easier event editing.