using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroEspinaca.Shared.Errors;
public abstract class DomainException : Exception { protected DomainException(string message) : base(message) { } }