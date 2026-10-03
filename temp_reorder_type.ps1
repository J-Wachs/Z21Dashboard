$ErrorActionPreference = 'Stop'
$file = Join-Path $PSScriptRoot 'Z21Dashboard\Resources\Raw\LocoDecoders\145-Zimo-MX633P22.json'

function MoveTypeFirst([System.Text.Json.Nodes.JsonNode]$node) {
	if ($node -is [System.Text.Json.Nodes.JsonObject]) {
		$ordered = [System.Text.Json.Nodes.JsonObject]::new()
		if ($node.ContainsKey('type')) {
			$ordered['type'] = $node['type'].DeepClone()
		}
		foreach ($prop in $node.AsObject()) {
			if ($prop.Key -ne 'type') {
				$ordered[$prop.Key] = MoveTypeFirst $prop.Value
			}
		}
		return $ordered
	}
	elseif ($node -is [System.Text.Json.Nodes.JsonArray]) {
		$arr = [System.Text.Json.Nodes.JsonArray]::new()
		foreach ($item in $node.AsArray()) {
			$arr.Add((MoveTypeFirst $item))
		}
		return $arr
	}
	return $node.DeepClone()
}

$json = [System.IO.File]::ReadAllText($file)
$root = [System.Text.Json.Nodes.JsonNode]::Parse($json)
$reordered = MoveTypeFirst $root
$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.WriteIndented = $true
[System.IO.File]::WriteAllText($file, $reordered.ToJsonString($options))
Write-Host "Done reordering 'type' properties in $file"
