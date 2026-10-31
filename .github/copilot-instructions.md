# Copilot Instructions

## Azure Guidelines
- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

## Code Style
- Always use C# 12 / .NET 10 collection expressions (e.g., `List<int> myList = [];`, `int[] a = [1,2,3];`) instead of explicit constructor assignment like `new()` or `new List<int>()`. This applies to all collection initializations in this codebase.
