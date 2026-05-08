using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

// ─────────────────────────────────────────────────────────────
//  MAX AI — Groq Runtime Script
//  Attach this to a GameObject in your scene if you want to
//  call MAX AI from inside your game at runtime.
//  For the Editor panel, use GeminiAIEditor.cs instead.
// ─────────────────────────────────────────────────────────────

public class GeminiAI : MonoBehaviour
{
    [Header("Groq API Settings")]
    public string apiKey = "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"; // starts with gsk_

    private string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private string model = "llama-3.3-70b-versatile";

    private string systemPrompt =
        "You are MAX — a world-class Unity game development mentor operating in 2026. " +
        "You are helping a beginner solo indie developer build Javelin Masters — a casual javelin throw mobile game in Unity 6. " +
        "Always give short, clear, copy-paste ready C# code. " +
        "Explain every concept simply like teaching a 16 year old. " +
        "Focus on Unity 6 best practices and mobile performance.";

    // ─────────────────────────────────────────────────────
    public void Ask(string userMessage, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(SendRequest(userMessage, onSuccess, onError));
    }

    IEnumerator SendRequest(string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        string escaped = userMessage
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");

        string escapedSystem = systemPrompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");

        string jsonBody =
            "{" +
                "\"model\":\"" + model + "\"," +
                "\"messages\":[" +
                    "{\"role\":\"system\",\"content\":\"" + escapedSystem + "\"}," +
                    "{\"role\":\"user\",\"content\":\"" + escaped + "\"}" +
                "]," +
                "\"max_tokens\":1024," +
                "\"temperature\":0.7" +
            "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string extracted = ExtractText(request.downloadHandler.text);
                onSuccess?.Invoke(extracted);
            }
            else
            {
                string err = "Error: " + request.error + "\n" + request.downloadHandler.text;
                Debug.LogError("[MAX AI] " + err);
                onError?.Invoke(err);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    string ExtractText(string json)
    {
        try
        {
            // Groq returns: choices[0].message.content
            string key = "\"content\":\"";
            int start = json.IndexOf(key);
            if (start == -1) { key = "\"content\": \""; start = json.IndexOf(key); }
            if (start == -1) return "Could not find content in response.";
            start += key.Length;
            int end = start;
            while (end < json.Length)
            {
                if (json[end] == '\\') { end += 2; continue; }
                if (json[end] == '"') break;
                end++;
            }
            return json.Substring(start, end - start)
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
        catch
        {
            return "Could not parse response.";
        }
    }
}
