using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalvrstaPickerViewModelTests
{
    [Fact]
    public void Picker_SortiraVrsteINormalizujeIzbor()
    {
        var vm = new NalvrstaPickerViewModel(
        [
            new NalvrstaRow { Vrnal = " 20", Naziv = "Druga" },
            new NalvrstaRow { Vrnal = " 10", Naziv = "Prva" },
        ]);

        Assert.Equal(new[] { "10", "20" },
            vm.Redovi.Select(r => r.Vrnal.Trim()).ToArray());
    }

    [Fact]
    public void NalbrojK2_PrimenjujeIzabranuVrstuSaTriMesta()
    {
        var red = new NalbrojRow();
        var vm = new NalbrojK2ViewModel(
            red, [red], new Dictionary<string, NalvrstaRow>());

        vm.PrimeniIzabranuVrstu("7");

        Assert.Equal("  7", vm.Vrnal);
    }
}
