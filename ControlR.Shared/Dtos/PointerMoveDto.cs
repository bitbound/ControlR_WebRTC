using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Dtos;

[DataContract]
public class PointerMoveDto
{
    public PointerMoveDto(double percentX, double percentY)
    {
        PercentX = percentX;
        PercentY = percentY;
    }

    [DataMember(Name = "percentX")]
    public double PercentX { get; init; }

    [DataMember(Name = "percentY")]
    public double PercentY { get; init; }
}
