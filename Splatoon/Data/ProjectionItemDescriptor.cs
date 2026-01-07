using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Data;

public class ProjectionItemDescriptor
{
    public ActionDescriptor Descriptor;
    public uint CasterObjectID;
    public bool IsBlacklisted;
    public List<string> BlacklistingLayouts = [];
    public List<string> WhitelistingLayouts = [];
    public List<string> SuppressingLayouts = [];
    public bool Rendered;

    public ProjectionItemDescriptor(ActionDescriptor descriptor, uint casterObjectID, bool isBlacklisted)
    {
        Descriptor = descriptor;
        CasterObjectID = casterObjectID;
        IsBlacklisted = isBlacklisted;
    }
}
