. "$PSScriptRoot/Environment.ps1"
# Player entry point: the genre shell (main scene). The internal console remains reachable from the start screen.
Start-Process -FilePath $Godot -ArgumentList @('--path',('"'+"$RepoRoot/game/Client"+'"'),'--',('"--content='+"$RepoRoot/content/fixture.json"+'"'))
