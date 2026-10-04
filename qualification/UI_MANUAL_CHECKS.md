# Remaining UI qualification checks

Run `QG02.ps1` for the instrumented synthetic benchmark, or launch the Godot
project with user argument `--ui` for interactive exploration. The built-in Tree
contains at most 100 data TreeItems plus its root; the dataset is plain C# data.

Manual follow-up on Director-approved hardware:

1. At each dataset size, sort every heading, combine Active only with name search,
   page forward/back, select a known ID, reorder/filter, and activate it. Confirm
   the status reports that ID; a selected ID excluded by filtering cannot act.
2. Use Tab/Shift+Tab, arrows and Enter. Confirm visible focus, mouse selection,
   horizontal/vertical scrolling, and no double activation after navigation.
3. Check 1280×720, 1920×1080 and 2560×1440 at Windows scaling 100/125/150/200%.
   Move between monitors of different DPI. Verify search, filters, navigation and
   actions remain reachable; inspect expanded names and unknown values.
4. Record OS, CPU/GPU, memory, build, monitor and actual OS scaling. Application
   ContentScaleFactor experiments are not substitutes for Windows DPI tests.

The automated input acknowledgement metric measures page binding through two
process frames; it is a responsiveness proxy, not physical input-to-photon
latency. Automated injected keyboard/mouse checks separately verify routing.
Memory samples are managed live bytes after collection, engine object counts,
and final process private bytes. They do not establish a production memory budget.
