# Web API
Splatoon now supports web API to remotely manage layouts and elements.
Request http://127.0.0.1:47774/ with parameters specified in table.
**Note: params are QueryString params, not JSON**
<table>
  <tr>
    <th>Parameter</td>
    <th>Usage</td>
  </tr>
  <tr>
    <td>enable</td>
    <td>Comma-separated names of already existing in Splatoon layouts or elements that you want to enable. If you want to enable layout simply pass it's name, if you want to enable specific element, use <code>layoutName~elementName</code> pattern.</td>
  </tr>
  <tr>
    <td>disable</td>
    <td>Same as <code>enable</code>, but will disable elements instead</td>
  </tr>
  <tr>
    <td colspan="2">Note: disabling always done before enabling. You can pass both parameters in one request. For example you can pass all known elements in disable parameter to clean up display, and then enable only ones that are currently needed. Passing same name of element in both <code>enable</code> and <code>disable</code> parameters will always result in element being enabled.</td>
  </tr>
  <tr>
    <td>elements</td>
    <td>Directly transfer encoded element into Splatoon without need of any preconfiguration from inside plugin. They are temporary and will not be stored by Splatoon between restarts.
<ul>
  <li>To obtain encoded layout/element, press <b>Copy as HTTP param</b> button inside Splatoon plugin. These buttons are located inside every layout and element.</li>
      <li> Multiple comma-separated values allowed.</li>
      <li> Can contain layouts and elements at the same time. To obtain layout/element code, use appropriate button inside Splatoon configuration after setting them up.</li>
      <li> If you are exporting layout, it's display conditions, zone/job lock, etc are preserved. If you are exporting element, no display conditions and locks will be attached to it. You do not need to enable layouts/elements before exporting, it will be done automatically.</li>
      </ul>
  </td>
  </tr>
  <tr>
    <td>namespace</td>
    <td>Add elements to specific named namespace instead of default one. If you are not using <code>destroyAt</code> parameter, always specify namespace so you can destroy element manually later. This will apply to all layouts/elements passed in current request. Namespaces are not unique: you can reuse same namespace in further queries to add more layouts/elements to a single namespace.</td>
  </tr>
  <tr>
    <td>destroyAt</td>
    <td>Passing this parameter let you specify when layouts/elements you have defined in request should be destroyed automatically by Splatoon. This parameter can take the following values:
  <ul>
    <li><code>NEVER</code> or <code>0</code> - do not use auto-destroy. This is default value. </li>
    <li><code>COMBAT_EXIT</code> - destroy layouts/elements next time player exits combat.</li>
    <li><code>TERRITORY_CHANGE</code> - destroy layouts/elements next time player changes territory (enters/exits dungeon, for example)</li>
    <li>Numeric value greater than 0 - destroy layouts/elements after this much <b>milli</b>seconds have passed.</li>
      </ul>
      This will apply to all layouts/elements passed in current request. <b>You can send multiple comma-separated values, as soon as any specified condition is met, elements will be removed.</b>
  </td>
  </tr>
  <tr>
    <td>destroy</td>
    <td>Comma-separated namespaces that will be destroyed. All elements that were added under namespace you specified will be destroyed at once. Destruction is always processed before addition of new layouts/elements, so if you want to clear your namespace from possible remainings from previous additions, just pass it's name in <code>destroy</code> parameter as well.</td>
  </tr>
  <tr>
    <td>raw</td>
    <td>By default you have to pass layouts/elements in encoded format. However that makes it difficult to edit from outside of Splatoon. Should you require this possibility - hold CTRL while copying layout/element from Splatoon to obtain it in urlencoded JSON format to which you can easily make changes and then pass it to <code>raw</code> parameter in your query. <b>Only one raw layout/element can be passed in a single query</b>, but you can freely pass encoded and raw at the same time.</td>
  </tr>
</table>
<b>In addition to all this, you may send element/layout inside POST request body in raw, non-encoded format. To get pretty-printed json of layout/element, hold ALT while pressing "Copy as HTTP param" button.</b> Only one layout/element per query in the body is allowed.
<br>
There is no difference between sending everything in one query and sending one layout/element per query. It also doesn't matters if you want to primarily use encoded or raw format. Just do it as you personally prefer.

## Examples for Triggernometry
Show standard/technical step radius while dancing: https://gist.github.com/Limiana/8788c387bfc5fcfd76499ef4e46d37d9
