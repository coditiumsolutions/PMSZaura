import React, { useEffect, useState } from "react";
import Aos from "aos";
import "aos/dist/aos.css";
import { Phone, MapPin, Mail, Send, LogIn, MessageCircle, Building2, Home } from "lucide-react";
import { useDarkMode } from "../Components/DarkModeContext";

const Contact = () => {
  const { darkMode } = useDarkMode();
  const [formData, setFormData] = useState({
    name: "",
    phone: "",
    email: "",
    type: "",
    message: "",
  });
  const [loginData, setLoginData] = useState({
    cnic: "",
    password: "",
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitMessage, setSubmitMessage] = useState("");

  // WhatsApp number - Update this with your company WhatsApp number
  const whatsappNumber = "+211922000000"; // Replace with your South Sudan number

  useEffect(() => {
    Aos.init({ offset: 150, duration: 800, easing: "ease-in-out", delay: 100 });
  }, []);

  const handleWhatsAppClick = () => {
    const message = encodeURIComponent(
      `Hello Juba Smart City,\n\nI'm interested in learning more about your plots and investment opportunities.\n\nName: ${formData.name || "[Your Name]"}\nPhone: ${formData.phone || "[Your Phone]"}\nEmail: ${formData.email || "[Your Email]"}\n\nMessage: ${formData.message || "I'd like to know more about available plots."}`
    );
    window.open(`https://wa.me/${whatsappNumber}?text=${message}`, '_blank');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsSubmitting(true);
    setSubmitMessage("");
    
    try {
      // Prepare request data
      const requestData = {
        FullName: formData.name,
        PhoneNumber: formData.phone,
        EmailAddress: formData.email,
        Type: formData.type,
        Message: formData.message
      };

      console.log('Request data:', requestData);

      // Try HTTP first since server is on port 80, then HTTPS if HTTP fails
      let apiUrl = 'http://app.virtualsofttechnology.com/api/InquiryApi/Submit?' + 
        new URLSearchParams({
          fullName: formData.name,
          phoneNumber: formData.phone,
          emailAddress: formData.email,
          type: formData.type,
          message: formData.message
        }).toString();
      
      let response;
      
      try {
        console.log('Trying HTTP:', apiUrl);
        response = await fetch(apiUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(10000) // 10 second timeout
        });
      } catch (httpError) {
        console.log('HTTP failed, trying HTTPS:', httpError.message);
        apiUrl = 'https://app.virtualsofttechnology.com/api/InquiryApi/Submit?' + 
          new URLSearchParams({
            fullName: formData.name,
            phoneNumber: formData.phone,
            emailAddress: formData.email,
            type: formData.type,
            message: formData.message
          }).toString();
        console.log('Trying HTTPS:', apiUrl);
        response = await fetch(apiUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(10000) // 10 second timeout
        });
      }

      console.log('Response status:', response.status);
      console.log('Response headers:', response.headers);

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      console.log('Response data:', result);

      if (result.success) {
        setSubmitMessage(`✅ ${result.message}`);
        setFormData({ name: "", phone: "", email: "", type: "", message: "" });
      } else {
        setSubmitMessage(`❌ ${result.message}`);
      }
    } catch (error) {
      console.error("Submission error:", error);
      console.error("Error details:", {
        name: error.name,
        message: error.message,
        stack: error.stack
      });
      
      if (error.name === 'TypeError' && error.message.includes('Failed to fetch')) {
        if (error.message.includes('timeout')) {
          setSubmitMessage("❌ Request timed out. The server may be slow or unreachable. Please try again.");
        } else if (error.message.includes('ERR_CONNECTION_TIMED_OUT')) {
          setSubmitMessage("❌ Connection timed out. Please check if the API server is running and accessible.");
        } else {
          setSubmitMessage("❌ Unable to connect to the server. Please check if the API is running and try again.");
        }
      } else {
        setSubmitMessage(`❌ Error submitting inquiry: ${error.message}`);
      }
    } finally {
      setIsSubmitting(false);
      setTimeout(() => setSubmitMessage(""), 8000);
    }
  };

  const handleLogin = async (e) => {
    e.preventDefault();
    setIsSubmitting(true);
    
    try {
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.log("Login Data:", loginData);
      alert("✅ Login successful! Redirecting to your dashboard...");
      setLoginData({ cnic: "", password: "" });
    } catch (error) {
      alert("❌ Invalid credentials. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      id="contact"
      className={`${
        darkMode ? "bg-gradient-to-br from-gray-900 to-gray-800 text-white" : "bg-gradient-to-br from-green-50/30 to-amber-50/30 text-gray-900"
      } pt-16 pb-4 transition-colors duration-500`}
    >
      <div className="container mx-auto px-4 sm:px-6 lg:px-12 xl:px-20 flex flex-col items-center gap-12">
        {/* Heading */}
        <div data-aos="fade-up" className="text-center max-w-2xl">
          <h1 className="text-4xl font-bold mb-4 border-l-4 border-green-600 pl-3 text-left">
            Contact Us
          </h1>
          <p className="text-lg text-left">
            Get in touch with our team in South Sudan. Visit our offices in Juba 
            and other locations for property inquiries, bookings, and customer support.
          </p>
        </div>

        {/* Main Grid */}
        <div className="grid lg:grid-cols-3 md:grid-cols-2 grid-cols-1 gap-10 w-full">
          {/* Online Booking Form */}
         
         <div
  data-aos="fade-right"
  className={`p-8 rounded-2xl shadow-xl border-2 ${
    darkMode
      ? "bg-gray-800 border-gray-700 hover:border-amber-500/50"
      : "bg-white border-gray-100 hover:border-amber-500/30"
  } transition-all duration-300`}
>
  <div className="flex items-center gap-3 mb-4">
    <div className="bg-gradient-to-br from-amber-600 to-green-600 p-3 rounded-xl">
      <Send className="text-white" size={24} />
    </div>
    <div>
      <h2 className="text-2xl font-bold">Property Inquiry</h2>
      <p
        className={`text-sm ${
          darkMode ? "text-gray-400" : "text-gray-600"
        }`}
      >
        Send us your details
      </p>
    </div>
  </div>

  <p
    className={`text-sm mb-5 ${
      darkMode ? "text-gray-400" : "text-gray-600"
    }`}
  >
    Share your information and our team will contact you for booking
    or investment details.
  </p>

  <form onSubmit={handleSubmit} className="flex flex-col gap-4">
    <input
      type="text"
      placeholder="Full Name *"
      required
      value={formData.name}
      onChange={(e) =>
        setFormData({ ...formData, name: e.target.value })
      }
      className={`p-4 rounded-xl border-2 outline-none transition-all ${
        darkMode
          ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500"
          : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
      }`}
    />

    <input
      type="tel"
      placeholder="Phone Number *"
      required
      value={formData.phone}
      onChange={(e) =>
        setFormData({ ...formData, phone: e.target.value })
      }
      className={`p-4 rounded-xl border-2 outline-none transition-all ${
        darkMode
          ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500"
          : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
      }`}
    />

    <input
      type="email"
      placeholder="Email Address *"
      required
      value={formData.email}
      onChange={(e) =>
        setFormData({ ...formData, email: e.target.value })
      }
      className={`p-4 rounded-xl border-2 outline-none transition-all ${
        darkMode
          ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500"
          : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
      }`}
    />

    <select
      value={formData.type}
      onChange={(e) =>
        setFormData({ ...formData, type: e.target.value })
      }
      className={`p-4 rounded-xl border-2 outline-none transition-all ${
        darkMode
          ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500"
          : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
      }`}
    >
      <option value="">Select Property Type</option>
      <option value="Residential Plot">Residential Plot</option>
      <option value="Commercial Property">Commercial Property</option>
      <option value="Luxury Villa">Luxury Villa</option>
      <option value="Investment Opportunity">
        Investment Opportunity
      </option>
      <option value="General Inquiry">General Inquiry</option>
    </select>

    <textarea
      rows="3"
      placeholder="Your Message *"
      required
      value={formData.message}
      onChange={(e) =>
        setFormData({ ...formData, message: e.target.value })
      }
      className={`p-4 rounded-xl border-2 outline-none transition-all ${
        darkMode
          ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500"
          : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
      }`}
    ></textarea>

    <button
      type="submit"
      disabled={isSubmitting}
      className="bg-gradient-to-r from-amber-600 to-green-600 hover:from-amber-700 hover:to-green-700 text-white font-bold py-4 rounded-xl transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-xl transform hover:scale-[1.02] flex items-center justify-center gap-2"
    >
      <Send size={20} />
      {isSubmitting ? "Submitting..." : "Submit Inquiry"}
    </button>

    {/* Success/Error Message */}
    {submitMessage && (
      <div
        className={`p-3 rounded-lg text-center text-sm font-semibold ${
          submitMessage.includes("✅")
            ? "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400"
            : "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400"
        }`}
      >
        {submitMessage}
      </div>
    )}
  </form>
</div>
           

          {/*  Login */}
          <div
            data-aos="fade-up"
            className={`p-8 rounded-2xl shadow-xl border-2 ${
              darkMode 
                ? "bg-gray-800 border-gray-700 hover:border-amber-500/50" 
                : "bg-white border-gray-100 hover:border-amber-500/30"
            } transition-all duration-300`}
          >
            <div className="flex items-center gap-3 mb-4">
              <div className="bg-gradient-to-br from-amber-600 to-green-600 p-3 rounded-xl">
                <LogIn className="text-white" size={24} />
              </div>
              <div>
                <h2 className="text-2xl font-bold">Resident Portal</h2>
                <p className={`text-sm ${darkMode ? "text-gray-400" : "text-gray-600"}`}>
                  Access your account
                </p>
              </div>
            </div>
            <p className={`text-sm mb-5 ${darkMode ? "text-gray-400" : "text-gray-600"}`}>
              View payment history, challans, and uploaded documents.
            </p>
            <form onSubmit={handleLogin} className="flex flex-col gap-4">
              <input
                type="text"
                placeholder="CNIC / Registration ID *"
                required
                value={loginData.cnic}
                onChange={(e) => setLoginData({ ...loginData, cnic: e.target.value })}
                className={`p-4 rounded-xl border-2 outline-none transition-all ${
                  darkMode 
                    ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500" 
                    : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
                }`}
              />
              <input
                type="password"
                placeholder="Password *"
                required
                value={loginData.password}
                onChange={(e) => setLoginData({ ...loginData, password: e.target.value })}
                className={`p-4 rounded-xl border-2 outline-none transition-all ${
                  darkMode 
                    ? "bg-gray-700 border-gray-600 text-white focus:border-amber-500" 
                    : "bg-gray-50 border-gray-200 text-gray-900 focus:border-amber-500"
                }`}
              />
              <button
                type="submit"
                disabled={isSubmitting}
                className="bg-gradient-to-r from-amber-600 to-green-600 hover:from-amber-700 hover:to-green-700 text-white font-bold py-4 rounded-xl transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-xl transform hover:scale-[1.02] flex items-center justify-center gap-2"
              >
                <LogIn size={20} />
                {isSubmitting ? "Logging in..." : "Login to Portal"}
              </button>
            </form>
          </div>

          {/* Contact Information */}
          <div
            data-aos="fade-left"
            className={`p-8 rounded-2xl shadow-xl border-2 ${
              darkMode 
                ? "bg-gray-800 border-gray-700 hover:border-green-500/50" 
                : "bg-white border-gray-100 hover:border-green-500/30"
            } transition-all duration-300`}
          >
            <div className="flex items-center gap-3 mb-6">
              <div className="bg-gradient-to-br from-green-600 to-amber-600 p-3 rounded-xl">
                <MapPin className="text-white" size={24} />
              </div>
              <div>
                <h2 className="text-2xl font-bold">Office Locations</h2>
                <p className={`text-sm ${darkMode ? "text-gray-400" : "text-gray-600"}`}>
                  Visit us in Juba
                </p>
              </div>
            </div>

            <div className="space-y-5">
              <div className={`p-4 rounded-xl ${darkMode ? "bg-gray-700/50" : ""}`}>
                <div className="flex items-start gap-3">
                  <div className="bg-gradient-to-br from-green-600 to-amber-600 p-2 rounded-lg">
                    <MapPin className="text-white" size={18} />
                  </div>
                  <div>
                    <p className="font-bold text-lg">Juba Head Office</p>
                    <p className={darkMode ? "text-gray-400" : "text-gray-600"}>
                      Juba City Center, Central Equatoria
                    </p>
                  </div>
                </div>
              </div>

              <div className={`p-4 rounded-xl ${darkMode ? "bg-gray-700/50" : ""}`}>
                <div className="flex items-start gap-3">
                  <div className="bg-gradient-to-br from-amber-600 to-green-600 p-2 rounded-lg">
                    <Building2 className="text-white" size={18} />
                  </div>
                  <div>
                    <p className="font-bold text-lg">Juba Sales Office</p>
                    <p className={darkMode ? "text-gray-400" : "text-gray-600"}>
                      Juba Business Hub, Central Equatoria
                    </p>
                  </div>
                </div>
              </div>

              <div className={`p-4 rounded-xl ${darkMode ? "bg-gray-700/50" : ""}`}>
                <div className="flex items-start gap-3">
                  <div className="bg-gradient-to-br from-green-600 to-amber-600 p-2 rounded-lg">
                    <Home className="text-white" size={18} />
                  </div>
                  <div>
                    <p className="font-bold text-lg">Juba Residential Office</p>
                    <p className={darkMode ? "text-gray-400" : "text-gray-600"}>
                      Juba Residential District, Central Equatoria
                    </p>
                  </div>
                </div>
              </div>

              <div className="pt-4 border-t-2 border-gray-200 dark:border-gray-700 space-y-3">
                <a
                  href="tel:+211911111111"
                  className={`flex items-center gap-3 p-3 rounded-lg transition-all ${
                    darkMode 
                      ? "hover:bg-gray-700 text-gray-300 hover:text-white" 
                      : "hover:bg-gray-100 text-gray-700 hover:text-green-600"
                  }`}
                >
                  <Phone className="text-green-600" size={20} />
                  <span className="font-semibold">+211 911 111 111</span>
                </a>

                <a
                  href="mailto:info@jubasociety.com"
                  className={`flex items-center gap-3 p-3 rounded-lg transition-all ${
                    darkMode 
                      ? "hover:bg-gray-700 text-gray-300 hover:text-white" 
                      : "hover:bg-gray-100 text-gray-700 hover:text-green-600"
                  }`}
                >
                  <Mail className="text-green-600" size={20} />
                  <span className="font-semibold">info@jubasociety.com</span>
                </a>
              </div>

              <div className="flex items-center gap-3 mt-4">
                <a
                  href="https://wa.me/211911111111"
                  target="_blank"
                  rel="noreferrer"
                  className="bg-green-500 text-white px-4 py-2 rounded-lg hover:bg-green-600 transition-all duration-300"
                >
                  WhatsApp Us
                </a>
              </div>
            </div>

            {/* map */}
            <div className="mt-6 overflow-hidden rounded-xl shadow-md">
              <iframe
                title="Office Location"
                src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3970.1234567890123!2d31.5825!3d4.8594!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x171f5b5b5b5b5b5b%3A0x1234567890abcdef!2sJuba%2C%20South%20Sudan!5e0!3m2!1sen!2s!4v1698681727103!5m2!1sen!2s"
                width="100%"
                height="200"
                allowFullScreen=""
                loading="lazy"
                className="border-0"
              ></iframe>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Contact;
