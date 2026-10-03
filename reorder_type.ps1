param(
	[string]$Path = "Z21Dashboard\Resources\Raw\LocoDecoders\145-Zimo-MX633P22.json"
)

# Recursively rebuild a JsonNode, moving a "type" property to index 0 in every object.
function Move-TypeFirst {
	param([System.Text.Json.Nodes.JsonNode]$node)

	if ($node -is [System.Text.Json.Nodes.JsonObject]) {
		$newObj = [System.Text.Json.Nodes.JsonObject]::new()

		foreach ($prop in $node) {
			$value = Move-TypeFirst -node $prop.Value
			if ($prop.Key -eq 'type') {
				$newObj.Insert(0, $prop.Key, $value)
			} else {
				$newObj.Add($prop.Key, $value)
			}
		}

		return $newObj
	}

	if ($node -is [System.Text.Json.Nodes.JsonArray]) {
		$newArr = [System.Text.Json.Nodes.JsonArray]::new()
		foreach ($item in $node) {
			$newArr.Add((Move-TypeFirst -node $item))
		}
		return $newArr
	}

	return $node
}

$fullPath = Resolve-Path $Path -ErrorAction Stop
$json = [System.IO.File]::ReadAllText($fullPath, [System.Text.Encoding]::UTF8)

$doc = [System.Text.Json.Nodes.JsonNode]::Parse(
	$json,
	[System.Text.Json.Nodes.JsonNodeOptions]::new(),
	[System.Text.Json.JsonDocumentOptions]::new()
)

$cvDefinitions = $doc['cv_definitions']
for ($i = 0; $i -lt $cvDefinitions.Count; $i++) {
	$cvDefinitions[$i] = Move-TypeFirst -node $cvDefinitions[$i]
}

$serializeOptions = [System.Text.Json.JsonSerializerOptions]::new()
$serializeOptions.WriteIndented = $true
$serializeOptions.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping

$output = $doc.ToJsonString($serializeOptions)
[System.IO.File]::WriteAllText($fullPath, $output, [System.Text.Encoding]::UTF8)

Write-Host "Done: $fullPath"
