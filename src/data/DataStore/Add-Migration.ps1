param([string]$name)

$startupProject = "$PSScriptRoot/../../interface/Cli/Cli.csproj"

if (Test-Path -LiteralPath .\Database\CompiledModel) {
      Remove-Item -LiteralPath .\Database\CompiledModel -Recurse
}

dotnet ef dbcontext optimize --precompile-queries --output-dir .\Database\CompiledModel -s $startupProject

if ($?) {
	dotnet ef migrations add $name -o .\Database\Migrations -s $startupProject
}
