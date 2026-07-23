Add-Type -AssemblyName System.Net.Http

$script:sessionId = $null
$script:client = $null

function Connect-MCP {
    $script:client = New-Object System.Net.Http.HttpClient
    $client.Timeout = New-Object System.TimeSpan(0, 0, 30)
    $initBody = @{ jsonrpc = "2.0"; id = 1; method = "initialize"; params = @{ protocolVersion = "2024-11-05"; capabilities = @{}; clientInfo = @{ name = "opencode"; version = "1.0" } } } | ConvertTo-Json
    $initMsg = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, "http://127.0.0.1:8081/mcp")
    $initMsg.Content = New-Object System.Net.Http.StringContent($initBody, [System.Text.Encoding]::UTF8, "application/json")
    $initMsg.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream") | Out-Null
    $initResp = $client.SendAsync($initMsg).Result
    foreach ($h in $initResp.Headers) { if ($h.Key -eq "mcp-session-id") { $script:sessionId = [string]$h.Value } }
    Write-Host "Connected. Session: $script:sessionId"
}

function Invoke-MCP($method, $params) {
    if (-not $script:sessionId) { Connect-MCP }
    $body = @{ jsonrpc = "2.0"; id = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds(); method = $method; params = $params } | ConvertTo-Json -Depth 10
    $msg = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, "http://127.0.0.1:8081/mcp")
    $msg.Content = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, "application/json")
    $msg.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream") | Out-Null
    $msg.Headers.TryAddWithoutValidation("mcp-session-id", $script:sessionId) | Out-Null
    $resp = $client.SendAsync($msg).Result
    $raw = $resp.Content.ReadAsStringAsync().Result
    $start = $raw.IndexOf('{'); $end = $raw.LastIndexOf('}')
    if ($start -ge 0 -and $end -gt $start) { $raw = $raw.Substring($start, $end - $start + 1) }
    return $raw | ConvertFrom-Json
}

# Convert materials via execute_code
$code = @"
var folders = new[] { "Assets/_DLNK/Resources", "Assets/Materials", "Assets/_DLNK/_DLNK Source/_DLNK Libraries" };
var shaderMap = new System.Collections.Generic.Dictionary<string, string>
{
    { "Standard", "Universal Render Pipeline/Lit" },
    { "Autodesk Interactive", "Universal Render Pipeline/Lit" },
    { "Legacy Shaders/Diffuse", "Universal Render Pipeline/Lit" },
    { "Legacy Shaders/Transparent/Diffuse", "Universal Render Pipeline/Lit" },
    { "Legacy Shaders/Bumped Diffuse", "Universal Render Pipeline/Lit" },
    { "Legacy Shaders/Transparent/Bumped Diffuse", "Universal Render Pipeline/Lit" },
    { "Legacy Shaders/Bumped Specular", "Universal Render Pipeline/Lit" },
    { "Mobile/Diffuse", "Universal Render Pipeline/Lit" },
    { "Mobile/Bumped Diffuse", "Universal Render Pipeline/Lit" },
    { "Mobile/Unlit (Supports Lightmap)", "Universal Render Pipeline/Simple Lit" },
    { "Unlit/Texture", "Universal Render Pipeline/Unlit" },
    { "Unlit/Color", "Universal Render Pipeline/Unlit" },
    { "Particles/Standard Surface", "Universal Render Pipeline/Particles/Lit" },
    { "Particles/Standard Unlit", "Universal Render Pipeline/Particles/Unlit" },
    { "Sprites/Default", "Universal Render Pipeline/2D/Sprite-Unlit-Default" },
};
int total = 0;
foreach (var folder in folders)
{
    if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning("Folder not found: " + folder); continue; }
    var guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { folder });
    foreach (var guid in guids)
    {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null || mat.shader == null) continue;
        if (mat.shader.name.Contains("Universal Render Pipeline") || mat.shader.name.Contains("URP")) continue;
        if (shaderMap.TryGetValue(mat.shader.name, out var target))
        {
            var newShader = Shader.Find(target);
            if (newShader != null)
            {
                // Copy common properties
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"));
                if (mat.HasProperty("_Color")) mat.SetColor("_BaseColor", mat.GetColor("_Color"));
                if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", mat.GetTexture("_BumpMap"));
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", mat.GetTexture("_EmissionMap"));
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", mat.GetColor("_EmissionColor"));
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Smoothness", mat.GetFloat("_Glossiness"));
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", mat.GetFloat("_Metallic"));
                mat.shader = newShader;
                total++;
            }
        }
    }
}
UnityEditor.AssetDatabase.SaveAssets();
Debug.Log($"[MCP-URP] Converted {total} materials to URP.");
"@

$result = Invoke-MCP "tools/call" @{name = "execute_code"; arguments = @{code = $code}}
$result | ConvertTo-Json -Depth 3

Write-Host "`n=== Console ==="
Start-Sleep -Seconds 2
$console = Invoke-MCP "tools/call" @{name = "read_console"; arguments = @{count = 20; types = @("error", "warning", "log")}}
$consoleResult = $console.result.content[0].text | ConvertFrom-Json
$consoleResult.data | ForEach-Object { Write-Host "$_" }