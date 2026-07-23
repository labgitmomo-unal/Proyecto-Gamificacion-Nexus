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
    # Extract JSON from SSE envelope
    $start = $raw.IndexOf('{')
    $end = $raw.LastIndexOf('}')
    if ($start -ge 0 -and $end -gt $start) { $raw = $raw.Substring($start, $end - $start + 1) }
    return $raw | ConvertFrom-Json
}
