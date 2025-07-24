using System.ComponentModel.DataAnnotations;

namespace staj_proje.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gerekli")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre gerekli")]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Ad gerekli")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad gerekli")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email gerekli")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi girin")]
        [StringLength(100)]
        public string Email { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string FullName => $"{FirstName} {LastName}";
    }
}